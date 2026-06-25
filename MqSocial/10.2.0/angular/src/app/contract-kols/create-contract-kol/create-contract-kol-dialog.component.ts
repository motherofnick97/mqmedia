import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    ContractKolServiceProxy,
    CreateContractKolDto,
    ContractKolStatus,
    ReceiveStatus,
    KolServiceProxy,
    KolDto,
    ContractServiceProxy,
    ContractDto,
} from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { DatePicker } from 'primeng/datepicker';
import moment from 'moment';

@Component({
    templateUrl: './create-contract-kol-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        Button,
        Select,
        InputNumber,
        InputText,
        Textarea,
        DatePicker,
    ],
})
export class CreateContractKolDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    entry = new CreateContractKolDto();
    airTime: Date | null = null;
    kols: KolDto[] = [];
    contracts: ContractDto[] = [];

    statusOptions = [
        { value: ContractKolStatus.Register, label: 'Đăng ký' },
        { value: ContractKolStatus.Approve, label: 'Duyệt đăng ký' },
        { value: ContractKolStatus.Processing, label: 'Đang tiến hành' },
        { value: ContractKolStatus.MktOk, label: 'Mkt duyệt' },
        { value: ContractKolStatus.DpmOk, label: 'Quản lý duyệt' },
        { value: ContractKolStatus.OnAir, label: 'Đã air' },
        { value: ContractKolStatus.Following, label: 'Theo dõi' },
        { value: ContractKolStatus.Paid, label: 'Đã thanh toán' },
        { value: ContractKolStatus.Done, label: 'Hoàn thành' },
        { value: ContractKolStatus.Cancel, label: 'Hủy' },
        { value: ContractKolStatus.Reject, label: 'Từ chối' },
    ];

    receiveStatusOptions = [
        { value: ReceiveStatus.NotShip, label: 'Chưa gửi' },
        { value: ReceiveStatus.Shipping, label: 'Đang gửi' },
        { value: ReceiveStatus.Received, label: 'Đã nhận' },
        { value: ReceiveStatus.NotReceived, label: 'Không nhận' },
    ];

    constructor(
        injector: Injector,
        public bsModalRef: BsModalRef,
        private _contractKolService: ContractKolServiceProxy,
        private _kolService: KolServiceProxy,
        private _contractService: ContractServiceProxy,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this._kolService.getAll(undefined, undefined, undefined, undefined, 0, 1000).subscribe((r) => {
            this.kols = r.items ?? [];
            this.cd.detectChanges();
        });
        this._contractService.getAll(undefined, undefined, undefined, undefined, 0, 1000).subscribe((r) => {
            this.contracts = r.items ?? [];
            this.cd.detectChanges();
        });
    }

    save(): void {
        this.saving = true;
        this.entry.airTime = this.airTime ? moment(this.airTime) : undefined;
        this._contractKolService.create(this.entry).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
