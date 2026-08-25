import { Component, Injector, OnInit, Input, ChangeDetectorRef } from '@angular/core';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    ContractKolServiceProxy,
    ContractKolDto,
    ContractKolStatus,
    ContractStatus,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Tag } from 'primeng/tag';
import { TableModule } from 'primeng/table';
import { PrimeTemplate } from 'primeng/api';
import { CommonModule } from '@angular/common';
import { AddContractKolDialogComponent } from './add-contract-kol-dialog.component';

@Component({
    templateUrl: './manage-contract-kols-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        LocalizePipe,
        Button,
        Tag,
        TableModule,
        PrimeTemplate,
        CommonModule,
    ],
})
export class ManageContractKolsDialogComponent extends AppComponentBase implements OnInit {
    @Input() contractId: string;
    @Input() contractName: string;
    @Input() contractStatus: ContractStatus;

    contractKols: ContractKolDto[] = [];
    kolMap: Record<string, string> = {};
    loading = false;

    readonly statusLabels: Record<number, string> = {
        [ContractKolStatus.Register]: 'Đăng ký',
        [ContractKolStatus.Approve]: 'Duyệt đăng ký',
        [ContractKolStatus.Processing]: 'Đang tiến hành',
        [ContractKolStatus.MktOk]: 'Mkt duyệt',
        [ContractKolStatus.DpmOk]: 'Quản lý duyệt',
        [ContractKolStatus.OnAir]: 'Đã air',
        [ContractKolStatus.Following]: 'Theo dõi',
        [ContractKolStatus.Paid]: 'Đã thanh toán',
        [ContractKolStatus.Done]: 'Hoàn thành',
        [ContractKolStatus.Cancel]: 'Hủy',
        [ContractKolStatus.Reject]: 'Từ chối',
    };

    readonly statusSeverities: Record<number, 'secondary' | 'info' | 'success' | 'warn' | 'danger'> = {
        [ContractKolStatus.Register]: 'secondary',
        [ContractKolStatus.Approve]: 'info',
        [ContractKolStatus.Processing]: 'info',
        [ContractKolStatus.MktOk]: 'warn',
        [ContractKolStatus.DpmOk]: 'warn',
        [ContractKolStatus.OnAir]: 'success',
        [ContractKolStatus.Following]: 'secondary',
        [ContractKolStatus.Paid]: 'success',
        [ContractKolStatus.Done]: 'success',
        [ContractKolStatus.Cancel]: 'danger',
        [ContractKolStatus.Reject]: 'danger',
    };

    constructor(
        injector: Injector,
        public bsModalRef: BsModalRef,
        private _contractKolService: ContractKolServiceProxy,
        private _modalService: BsModalService,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.loadKols();
    }

    get isContractDone(): boolean {
        return this.contractStatus === ContractStatus.Complete;
    }

    loadKols(): void {
        this.loading = true;
        this._contractKolService
            .getAll(undefined, this.contractId, undefined, undefined, 0, 1000)
            .subscribe({
                next: (result) => {
                    this.contractKols = result.items ?? [];
                    this.contractKols.forEach((ck) => {
                        if (ck.kolId) this.kolMap[ck.kolId] = ck.kolName ?? ck.kolId;
                    });
                    this.loading = false;
                    this.cd.detectChanges();
                },
                error: () => { this.loading = false; },
            });
    }

    addKol(): void {
        if (this.isContractDone) {
            abp.notify.warn('Hợp đồng đã hoàn thành, không thể thêm KOL.');
            return;
        }
        const modalRef: BsModalRef = this._modalService.show(AddContractKolDialogComponent, {
            class: 'modal-xl',
            initialState: { contractId: this.contractId },
        });
        modalRef.content.onSave.subscribe(() => this.loadKols());
    }

    removeKol(ck: ContractKolDto): void {
        if (this.isContractDone) {
            abp.notify.warn('Hợp đồng đã hoàn thành, không thể xóa KOL.');
            return;
        }
        abp.message.confirm(
            `Xóa KOL "${this.getKolName(ck.kolId)}" khỏi hợp đồng?`,
            undefined,
            (ok: boolean) => {
                if (ok) {
                    this._contractKolService.delete(ck.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.loadKols();
                    });
                }
            }
        );
    }

    getKolName(kolId: string | undefined): string {
        if (!kolId) return '—';
        return this.kolMap[kolId] ?? kolId;
    }

    getStatusLabel(status: ContractKolStatus): string {
        return this.statusLabels[status] ?? '';
    }

    getStatusSeverity(status: ContractKolStatus): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
        return this.statusSeverities[status] ?? 'secondary';
    }
}
