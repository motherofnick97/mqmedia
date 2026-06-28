import { ChangeDetectorRef, Component, Injector, ViewChild } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase } from 'shared/paged-listing-component-base';
import { CareerServiceProxy, CareerDto, CareerDtoPagedResultDto } from '@shared/service-proxies/service-proxies';
import { CreateCareerDialogComponent } from './create-career/create-career-dialog.component';
import { EditCareerDialogComponent } from './edit-career/edit-career-dialog.component';
import { Table, TableModule } from 'primeng/table';
import { LazyLoadEvent, PrimeTemplate } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Button } from 'primeng/button';
import { Toolbar } from 'primeng/toolbar';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { Tooltip } from 'primeng/tooltip';

@Component({
    templateUrl: './careers.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [
        FormsModule,
        TableModule,
        PrimeTemplate,
        PaginatorModule,
        LocalizePipe,
        Button,
        Toolbar,
        IconField,
        InputIcon,
        InputText,
        Tooltip,
    ],
})
export class CareersComponent extends PagedListingComponentBase<CareerDto> {
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;

    keyword = '';

    constructor(
        injector: Injector,
        private _careerService: CareerServiceProxy,
        private _modalService: BsModalService,
        cd: ChangeDetectorRef
    ) {
        super(injector, cd);
    }

    createCareer(): void {
        const modalRef: BsModalRef = this._modalService.show(CreateCareerDialogComponent, { class: 'modal-md' });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    editCareer(career: CareerDto): void {
        const modalRef: BsModalRef = this._modalService.show(EditCareerDialogComponent, {
            class: 'modal-md',
            initialState: { id: career.id },
        });
        modalRef.content.onSave.subscribe(() => this.refresh());
    }

    list(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator.changePage(0);
            if (this.primengTableHelper.records && this.primengTableHelper.records.length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        this._careerService
            .getAll(
                this.keyword,
                this.primengTableHelper.getSorting(this.dataTable),
                this.primengTableHelper.getSkipCount(this.paginator, event),
                this.primengTableHelper.getMaxResultCount(this.paginator, event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result: CareerDtoPagedResultDto) => {
                this.primengTableHelper.records = result.items;
                this.primengTableHelper.totalRecordsCount = result.totalCount;
                this.primengTableHelper.hideLoadingIndicator();
                this.cd.detectChanges();
            });
    }

    delete(career: CareerDto): void {
        abp.message.confirm(
            `Xóa nghề nghiệp "${career.name}"?`,
            undefined,
            (result: boolean) => {
                if (result) {
                    this._careerService.delete(career.id).subscribe(() => {
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                        this.refresh();
                    });
                }
            }
        );
    }
}
